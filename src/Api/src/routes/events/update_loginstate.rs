use super::{PlayerEventStreamUpdate, PlayerStateUpdateType};
use crate::AppState;
use axum::{extract::State, http::StatusCode};
use axum_msgpack::MsgPack;
use serde::Deserialize;
use tracing::info;

const CONTENT_ID_HASH_LENGTH: usize = 64;
const CONTENT_ID_SALT_LENGTH: usize = 32;

#[derive(Debug, Deserialize)]
pub struct UpdatePlayerLoginStateRequest {
    pub content_id_hash: Box<str>,
    pub content_id_salt: Box<str>,
    pub logged_in: bool,
    pub territory_id: u16,
    pub world_id: u32,
}

pub async fn send_loginstate_handler(
    State(state): State<AppState>,
    MsgPack(update): MsgPack<UpdatePlayerLoginStateRequest>,
) -> StatusCode {
    if update.content_id_hash.len() < CONTENT_ID_HASH_LENGTH
        || update.content_id_salt.len() < CONTENT_ID_SALT_LENGTH
    {
        return StatusCode::BAD_REQUEST;
    }

    let _ = state
        .events_broadcast_channel
        .send(PlayerEventStreamUpdate {
            content_id_hash: update.content_id_hash,
            content_id_salt: update.content_id_salt,
            state_update_type: PlayerStateUpdateType::LoginStateChange {
                world_id: update.world_id,
                territory_id: update.territory_id,
                logged_in: update.logged_in,
            },
        });
    info!(
        "Sent event to {} subscribers",
        state.events_broadcast_channel.receiver_count()
    );
    StatusCode::ACCEPTED
}
