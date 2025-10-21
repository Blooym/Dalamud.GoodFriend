use crate::AppState;
use axum::{body::Body, extract::State, http::header, response::IntoResponse};
use std::convert::Infallible;
use tokio_stream::{StreamExt, wrappers::BroadcastStream};

pub async fn player_events_sse_handler(State(state): State<AppState>) -> impl IntoResponse {
    let rx = state.events_broadcast_channel.subscribe();
    let stream = BroadcastStream::new(rx).filter_map(|msg| match msg {
        Ok(data) => match rmp_serde::to_vec(&data) {
            Ok(bytes) => Some(Ok::<_, Infallible>(bytes)),
            Err(_) => None,
        },
        Err(_) => None,
    });
    (
        [
            (header::CONTENT_TYPE, "application/msgpack"),
            (header::CACHE_CONTROL, "no-cache"),
        ],
        Body::from_stream(stream),
    )
}
