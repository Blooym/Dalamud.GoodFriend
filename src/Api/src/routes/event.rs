use crate::{
    AppState,
    routes::{EventStreamData, EventStreamMessage},
};
use axum::{extract::State, http::StatusCode};
use axum_msgpack::MsgPack;
use serde::Deserialize;
use tracing::info;

#[derive(Deserialize)]
pub struct UpdatePlayerLoginStateRequest {
    #[serde(with = "serde_bytes")]
    pub content_id_hash: [u8; 32],
    #[serde(with = "serde_bytes")]
    pub content_id_salt: [u8; 16],
    pub logged_in: bool,
    pub territory_id: u16,
    pub world_id: u16,
}

pub async fn send_event_handler(
    State(state): State<AppState>,
    MsgPack(update): MsgPack<UpdatePlayerLoginStateRequest>,
) -> StatusCode {
    let _ = state
        .events_broadcast_channel
        .send(EventStreamMessage::Data(EventStreamData {
            content_id_hash: update.content_id_hash,
            content_id_salt: update.content_id_salt,
            world_id: update.world_id,
            territory_id: update.territory_id,
            logged_in: update.logged_in,
        }));

    info!(
        "Sent event to {} subscribers",
        state.events_broadcast_channel.receiver_count()
    );
    StatusCode::ACCEPTED
}
