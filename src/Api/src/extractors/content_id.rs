use axum::{
    extract::FromRequestParts,
    http::{HeaderMap, StatusCode, request::Parts},
    response::{IntoResponse, Response},
};
use moka::future::Cache;
use std::sync::{Arc, LazyLock};

const CONTENT_ID_HASH_HEADER: &str = "x-content-id-hash";
const CONTENT_ID_SALT_HEADER: &str = "x-content-id-salt";
const CONTENT_ID_HASH_LENGTH: usize = 64;
const CONTENT_ID_SALT_LENGTH: usize = 32;

static CONTENT_ID_HASH_CACHE: LazyLock<Arc<Cache<Box<str>, ()>>> = LazyLock::new(|| {
    const DEFAULT_CACHE_CAPACITY: u64 = 1000;
    Arc::new(Cache::new(
        std::env::var("GOODFRIEND_CONTENT_ID_CACHE_CAPACITY")
            .map(|v| v.parse().unwrap_or(DEFAULT_CACHE_CAPACITY))
            .unwrap_or(DEFAULT_CACHE_CAPACITY),
    ))
});

pub struct UniqueContentId {
    pub hash: Box<str>,
    pub salt: Box<str>,
}

#[derive(Debug)]
pub enum ContentIdExtractError {
    HashMissing,
    SaltMissing,
    HashOrSaltInvalid,
    ContentIdDuplicate,
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

        // Ensure duplicate ContentId requests are not processed.
        if CONTENT_ID_HASH_CACHE.contains_key(&content_id_hash) {
            return Err(ContentIdExtractError::ContentIdDuplicate);
        }
        CONTENT_ID_HASH_CACHE
            .insert(content_id_hash.clone(), ())
            .await;

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
            Self::ContentIdDuplicate => (StatusCode::CONFLICT, "Duplicated ContentID request data"),
        }
        .into_response()
    }
}
