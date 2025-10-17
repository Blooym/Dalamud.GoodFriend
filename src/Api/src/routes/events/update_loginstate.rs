use super::{PlayerEventStreamUpdate, PlayerStateUpdateType};
use crate::{
    AppState,
    extractors::{ClientKey, UniqueContentId},
};
use axum::{extract::State, http::StatusCode};
use axum_msgpack::MsgPack;
use serde::Deserialize;
use tracing::info;

#[derive(Debug, Deserialize)]
pub struct UpdatePlayerLoginStateRequest {
    pub logged_in: bool,
    pub territory_id: u16,
    pub world_id: u32,
}

pub async fn send_loginstate_handler(
    _client_key: ClientKey,
    content_id: UniqueContentId,
    State(state): State<AppState>,
    MsgPack(update): MsgPack<UpdatePlayerLoginStateRequest>,
) -> StatusCode {
    let _ = state.player_events_stream.send(PlayerEventStreamUpdate {
        content_id_hash: content_id.hash,
        content_id_salt: content_id.salt,
        state_update_type: PlayerStateUpdateType::LoginStateChange {
            world_id: update.world_id,
            territory_id: update.territory_id,
            logged_in: update.logged_in,
        },
    });
    info!(
        "Sent loginstate event to {} subscribers",
        state.player_events_stream.receiver_count()
    );
    StatusCode::ACCEPTED
}
