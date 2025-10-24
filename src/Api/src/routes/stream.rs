use crate::AppState;
use anyhow::Result;
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

#[derive(Clone, Serialize)]
pub struct EventData {
    #[serde(with = "serde_bytes")]
    pub content_id_hash: [u8; 32],
    #[serde(with = "serde_bytes")]
    pub content_id_salt: [u8; 16],
    pub logged_in: bool,
    pub territory_id: u16,
    pub world_id: u16,
}

#[derive(Clone)]
pub enum EventStreamMessage {
    Data(SerializedEventData),
    Shutdown,
}

#[derive(Clone)]
pub struct SerializedEventData(Vec<u8>);

impl SerializedEventData {
    pub fn new(data: &EventData) -> Result<Self, rmp_serde::encode::Error> {
        let serialized = rmp_serde::to_vec(data)?;
        Ok(SerializedEventData(serialized))
    }
}

pub async fn event_stream_handler(State(state): State<AppState>) -> impl IntoResponse {
    const HEARTBEAT_INTERVAL: Duration = Duration::from_secs(30);
    const HEARTBEAT_DATA: &[u8] = &[0x90]; // An empty MessagePack object.
    let rx = state.events_broadcast_channel.subscribe();

    // Setup 3 streams:
    // - One-event stream that sends empty data to establish the connection.
    // - Interval stream that periodically send heartbeats to keep connections alive.
    // - Listener stream that handles sending events to clients and listens for shutdowns.
    // Merge them all into a single stream and send it.
    let initial_heartbeat = tokio_stream::once((HEARTBEAT_DATA.to_vec(), false));
    let heartbeat_stream = IntervalStream::new(time::interval(HEARTBEAT_INTERVAL))
        .map(|_| (HEARTBEAT_DATA.to_vec(), false));
    let event_stream = BroadcastStream::new(rx).filter_map(move |msg| match msg.ok()? {
        EventStreamMessage::Data(data) => Some((data.0, false)),
        EventStreamMessage::Shutdown => Some((vec![], true)),
    });
    let stream = initial_heartbeat
        .chain(StreamExt::merge(event_stream, heartbeat_stream))
        .take_while(|(_, is_shutdown)| !is_shutdown)
        .map(|(data, _)| Ok::<_, Infallible>(data));

    (
        [
            (header::CONTENT_TYPE, "application/msgpack"),
            (header::CACHE_CONTROL, "no-store, no-cache, no-transform"),
            (HeaderName::from_static("x-accel-buffering"), "no"),
        ],
        Body::from_stream(stream),
    )
}
