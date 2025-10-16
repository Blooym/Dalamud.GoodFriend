mod send;
mod stream;

pub use send::*;
pub use stream::*;

use serde::{Deserialize, Serialize};
use uuid::Uuid;

#[derive(Debug, Clone, Serialize, Deserialize)]
enum AnnouncementKind {
    Informational,
    Maintenance,
    Critical,
    Miscellaneous,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
enum AnnouncementCause {
    Manual,
    Automatic,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AnnouncementMessage {
    #[serde(skip_serializing_if = "Option::is_none")]
    id: Option<Uuid>,
    message: Box<str>,
    kind: AnnouncementKind,
    cause: AnnouncementCause,
    #[serde(skip_serializing_if = "Option::is_none")]
    channel: Option<Box<str>>,
}
