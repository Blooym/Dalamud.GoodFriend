mod send;
mod stream;

pub use send::*;
pub use stream::*;

use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
enum AnnouncementKind {
    Informational,
    Maintenance,
    Critical,
    Miscellaneous,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AnnouncementMessage {
    message: Box<str>,
    kind: AnnouncementKind,
    #[serde(skip_serializing_if = "Option::is_none")]
    channel: Option<Box<str>>,
}
