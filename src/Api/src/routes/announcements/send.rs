use super::AnnouncementMessage;
use crate::{AppState, extractors::AuthenticatedUser};
use axum::{extract::State, http::StatusCode};
use axum_msgpack::MsgPack;
use tracing::info;

pub async fn post_announcement(
    _user: AuthenticatedUser,
    State(state): State<AppState>,
    MsgPack(payload): MsgPack<AnnouncementMessage>,
) -> StatusCode {
    if payload.message.trim().is_empty() {
        return StatusCode::BAD_REQUEST;
    }

    if state.announcement_events_stream.send(payload).is_err() {
        return StatusCode::INTERNAL_SERVER_ERROR;
    }

    info!(
        "Sent announcement event to {} subscribers",
        state.announcement_events_stream.receiver_count()
    );

    StatusCode::OK
}
