use crate::AppState;
use axum::{
    body::Body,
    extract::State,
    http::{HeaderName, header},
    response::IntoResponse,
};
use serde::Serialize;
use std::{convert::Infallible, time::Duration};
use tokio::time;
use tokio_stream::{
    StreamExt,
    wrappers::{BroadcastStream, IntervalStream},
};

#[derive(Clone)]
pub enum EventStreamMessage {
    Data(EventStreamData),
    Shutdown,
}

#[derive(Clone, Serialize)]
pub struct EventStreamData {
    #[serde(with = "serde_bytes")]
    pub content_id_hash: [u8; 32],
    #[serde(with = "serde_bytes")]
    pub content_id_salt: [u8; 16],
    pub logged_in: bool,
    pub territory_id: u16,
    pub world_id: u16,
}

pub async fn event_stream_handler(State(state): State<AppState>) -> impl IntoResponse {
    const HEARTBEAT_INTERVAL: Duration = Duration::from_secs(30);
    let rx = state.events_broadcast_channel.subscribe();

    // Instantly send empty data to establish the connection and avoid timeouts.
    let initial_heartbeat = tokio_stream::once((vec![0x90], false));
    // Periodically send heartbeats to ensure that all clients are still connected.
    let heartbeat_stream =
        IntervalStream::new(time::interval(HEARTBEAT_INTERVAL)).map(|_| (vec![0x90], false));
    // Handle sending events to clients and listen for shutdowns.
    let event_stream = BroadcastStream::new(rx).filter_map(move |msg| match msg.ok()? {
        EventStreamMessage::Data(data) => rmp_serde::to_vec(&data).ok().map(|bytes| (bytes, false)),
        EventStreamMessage::Shutdown => Some((vec![], true)),
    });
    // Merge all streams into one.
    let merged_stream = initial_heartbeat
        .chain(StreamExt::merge(event_stream, heartbeat_stream))
        .take_while(|(_, is_shutdown)| !is_shutdown)
        .map(|(data, _)| Ok::<_, Infallible>(data));
    // Send stream.
    (
        [
            (header::CONTENT_TYPE, "application/msgpack"),
            (header::CACHE_CONTROL, "no-store, no-cache, no-transform"),
            (HeaderName::from_static("x-accel-buffering"), "no"),
        ],
        Body::from_stream(merged_stream),
    )
}
