use axum::{
    extract::FromRequestParts,
    http::{HeaderMap, StatusCode, request::Parts},
    response::{IntoResponse, Response},
};

const CONTENT_ID_HASH_HEADER: &str = "x-content-id-hash";
const CONTENT_ID_SALT_HEADER: &str = "x-content-id-salt";
const CONTENT_ID_HASH_LENGTH: usize = 64;
const CONTENT_ID_SALT_LENGTH: usize = 32;

pub struct UniqueContentId {
    pub hash: Box<str>,
    pub salt: Box<str>,
}

#[derive(Debug)]
pub enum ContentIdExtractError {
    HashMissing,
    SaltMissing,
    HashOrSaltInvalid,
}

impl<S> FromRequestParts<S> for UniqueContentId
where
    S: Send + Sync,
{
    type Rejection = ContentIdExtractError;

    async fn from_request_parts(parts: &mut Parts, _state: &S) -> Result<Self, Self::Rejection> {
        let headers: &HeaderMap = &parts.headers;
        let content_id_hash: Box<str> = headers
            .get(CONTENT_ID_HASH_HEADER)
            .and_then(|v| v.to_str().ok())
            .ok_or(ContentIdExtractError::HashMissing)?
            .into();
        let content_id_salt: Box<str> = headers
            .get(CONTENT_ID_SALT_HEADER)
            .and_then(|v| v.to_str().ok())
            .ok_or(ContentIdExtractError::SaltMissing)?
            .into();

        if content_id_hash.len() < CONTENT_ID_HASH_LENGTH
            || content_id_salt.len() < CONTENT_ID_SALT_LENGTH
        {
            return Err(ContentIdExtractError::HashOrSaltInvalid);
        }

        Ok(Self {
            hash: content_id_hash,
            salt: content_id_salt,
        })
    }
}

impl IntoResponse for ContentIdExtractError {
    fn into_response(self) -> Response {
        match self {
            Self::HashMissing | Self::SaltMissing | Self::HashOrSaltInvalid => {
                (StatusCode::BAD_REQUEST, "Invalid ContentId data")
            }
        }
        .into_response()
    }
}
