mod extractors;
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
use routes::{
    AnnouncementMessage, PlayerEventStreamUpdate, announcement_event_sse_handler, health_handler,
    player_events_sse_handler, post_announcement, send_loginstate_handler, validate_auth_handler,
};
use tokio::{net::TcpListener, signal, sync::broadcast};
use tower_http::{
    catch_panic::CatchPanicLayer,
    normalize_path::NormalizePathLayer,
    trace::{self, DefaultOnFailure, DefaultOnRequest, DefaultOnResponse, TraceLayer},
};
use tracing::{Level, info};
use tracing_subscriber::EnvFilter;

#[derive(Debug, Clone, Parser)]
#[clap(author, about, version)]
struct Arguments {
    /// Internet socket address that the server should be ran on.
    #[arg(
        long = "address",
        env = "GOODFRIEND_API_ADDRESS",
        default_value = "127.0.0.1:8001"
    )]
    address: SocketAddr,

    /// Authentication tokens for use with authenticated endpoints.
    #[arg(
        long = "api-auth-tokens",
        env = "GOODFRIEND_API_AUTH_TOKENS",
        value_delimiter = ','
    )]
    pub authentication_tokens: Vec<String>,

    /// Client keys to use to restrict API usage to allowed clients only.
    #[arg(
        long = "api-client-keys",
        env = "GOODFRIEND_API_CLIENT_KEYS",
        value_delimiter = ','
    )]
    pub allowed_client_keys: Option<Vec<String>>,

    /// The capacity of the 'announcements' SSE stream. This should be kept close to `player_sse_cap-sse-cap`.
    #[arg(
        long = "api-announce-sse-cap",
        env = "GOODFRIEND_API_ANNOUNCESSE_CAP",
        default_value_t = 10000
    )]
    pub announce_sse_cap: usize,

    /// The capacity of the 'player events' SSE stream. This should be kept close to `announce-sse-cap`.
    #[arg(
        long = "api-player-sse-cap",
        env = "GOODFRIEND_API_PLAYERSSE_CAP",
        default_value_t = 10000
    )]
    pub player_sse_cap: usize,
}

#[derive(Clone)]
struct AppState {
    player_events_stream: broadcast::Sender<PlayerEventStreamUpdate>,
    announcement_events_stream: broadcast::Sender<AnnouncementMessage>,
    authentication_tokens: Box<[String]>,
    client_keys: Option<Box<[String]>>,
}

#[tokio::main]
async fn main() -> Result<()> {
    dotenv().ok();
    tracing_subscriber::fmt()
        .with_env_filter(EnvFilter::try_from_default_env().unwrap_or(EnvFilter::new("info")))
        .init();
    let args = Arguments::parse();

    // Start server.
    let app_state = AppState {
        player_events_stream: broadcast::channel::<PlayerEventStreamUpdate>(args.player_sse_cap).0,
        announcement_events_stream: broadcast::channel::<AnnouncementMessage>(
            args.announce_sse_cap,
        )
        .0,
        authentication_tokens: args.authentication_tokens.into(),
        client_keys: args.allowed_client_keys.map(core::convert::Into::into),
    };

    let tcp_listener = TcpListener::bind(args.address).await?;
    let router = Router::new()
        .route("/api/health", get(health_handler))
        .route("/api/auth/validate", post(validate_auth_handler))
        .route("/api/announcements/send", post(post_announcement))
        .route(
            "/api/announcements/stream",
            get(announcement_event_sse_handler),
        )
        .route(
            "/api/playerevents/loginstate",
            post(send_loginstate_handler),
        )
        .route("/api/playerevents/stream", get(player_events_sse_handler))
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

    info!(
        "Internal server started\n\
         * Listening on: http://{}",
        args.address,
    );
    axum::serve(tcp_listener, router)
        .with_graceful_shutdown(shutdown_signal())
        .await?;
    Ok(())
}

// https://github.com/tokio-rs/axum/blob/15917c6dbcb4a48707a20e9cfd021992a279a662/examples/graceful-shutdown/src/main.rs#L55
async fn shutdown_signal() {
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
}
