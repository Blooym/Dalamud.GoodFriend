mod routes;

use anyhow::Result;
use axum::{
    Router,
    extract::Request,
    http::{HeaderValue, header},
    middleware::{self as axum_middleware, Next},
    routing::{get, post},
};
use clap::Parser;
use core::net::SocketAddr;
use dotenvy::dotenv;
use routes::{EventStreamMessage, event_stream_handler, health_handler, send_event_handler};
use tokio::{net::TcpListener, signal, sync::broadcast};
use tower_http::{
    catch_panic::CatchPanicLayer,
    normalize_path::NormalizePathLayer,
    trace::{self, DefaultOnFailure, DefaultOnRequest, DefaultOnResponse, TraceLayer},
};
use tracing::{Level, info};
use tracing_subscriber::EnvFilter;

#[derive(Parser)]
#[clap(author, about, long_about, version)]
struct Arguments {
    /// Internet socket address that the server should be ran on.
    #[arg(
        long = "address",
        env = "GOODFRIEND_API_ADDRESS",
        default_value = "127.0.0.1:8001"
    )]
    address: SocketAddr,
}

#[derive(Clone)]
struct AppState {
    events_broadcast_channel: broadcast::Sender<EventStreamMessage>,
}

#[tokio::main]
async fn main() -> Result<()> {
    dotenv().ok();
    tracing_subscriber::fmt()
        .with_env_filter(EnvFilter::try_from_default_env().unwrap_or(EnvFilter::new("info")))
        .init();
    let args = Arguments::parse();

    // Start server.
    let stream_channel = broadcast::channel::<EventStreamMessage>(16).0;
    let app_state = AppState {
        events_broadcast_channel: stream_channel.clone(),
    };
    let tcp_listener = TcpListener::bind(args.address).await?;
    let router = Router::new()
        .route("/api/health", get(health_handler))
        .route("/api/event", post(send_event_handler))
        .route("/api/stream", get(event_stream_handler))
        .layer(
            TraceLayer::new_for_http()
                .make_span_with(trace::DefaultMakeSpan::new().level(Level::INFO))
                .on_request(DefaultOnRequest::default().level(Level::INFO))
                .on_response(DefaultOnResponse::default().level(Level::INFO))
                .on_failure(DefaultOnFailure::default().level(Level::INFO)),
        )
        .with_state(app_state)
        .layer(NormalizePathLayer::trim_trailing_slash())
        .layer(CatchPanicLayer::new())
        .layer(axum_middleware::from_fn(
            async |req: Request, next: Next| {
                let mut res = next.run(req).await;
                let res_headers = res.headers_mut();
                res_headers.insert(
                    header::SERVER,
                    HeaderValue::from_static(env!("CARGO_PKG_NAME")),
                );
                res_headers.insert("X-Robots-Tag", HeaderValue::from_static("none"));
                res
            },
        ));

    info!("Internal server started at http://{}", args.address);
    axum::serve(tcp_listener, router)
        .with_graceful_shutdown(shutdown_signal(stream_channel))
        .await?;
    Ok(())
}

// https://github.com/tokio-rs/axum/blob/15917c6dbcb4a48707a20e9cfd021992a279a662/examples/graceful-shutdown/src/main.rs#L55
async fn shutdown_signal(channel: broadcast::Sender<EventStreamMessage>) {
    let ctrl_c = async {
        signal::ctrl_c()
            .await
            .expect("failed to install Ctrl+C handler");
    };

    #[cfg(unix)]
    let terminate = async {
        signal::unix::signal(signal::unix::SignalKind::terminate())
            .expect("failed to install signal handler")
            .recv()
            .await;
    };

    #[cfg(not(unix))]
    let terminate = std::future::pending::<()>();

    tokio::select! {
        _ = ctrl_c => {},
        _ = terminate => {},
    }

    let _ = channel.send(EventStreamMessage::Shutdown);
}
