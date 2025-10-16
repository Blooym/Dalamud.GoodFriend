mod stream;
mod update_loginstate;

pub use stream::*;
pub use update_loginstate::*;

use serde::Serialize;

#[derive(Debug, Clone, Serialize)]
pub struct PlayerEventStreamUpdate {
    pub content_id_hash: Box<str>,
    pub content_id_salt: Box<str>,
    pub state_update_type: PlayerStateUpdateType,
}

#[derive(Debug, Clone, Serialize)]
pub enum PlayerStateUpdateType {
    LoginStateChange {
        world_id: u32,
        territory_id: u16,
        logged_in: bool,
    },
}
