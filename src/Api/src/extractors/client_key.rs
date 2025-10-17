use crate::AppState;
use axum::{
    extract::{FromRef, FromRequestParts},
    http::{StatusCode, request::Parts},
    response::{IntoResponse, Response},
};

const CLIENT_KEY_HEADER: &str = "X-Client-Key";

// Struct to represent the `ClientKey`
pub struct ClientKey;

#[derive(Debug)]
pub enum ClientKeyExtractError {
    InvalidKey,
    MissingKey,
}

impl<S> FromRequestParts<S> for ClientKey
where
    AppState: FromRef<S>,
    S: Send + Sync,
{
    type Rejection = ClientKeyExtractError;

    async fn from_request_parts(parts: &mut Parts, state: &S) -> Result<Self, Self::Rejection> {
        let state = AppState::from_ref(state);
        let Some(client_keys) = state.client_keys else {
            return Ok(ClientKey);
        };
        let Some(key) = parts
            .headers
            .get(CLIENT_KEY_HEADER)
            .and_then(|value| value.to_str().ok())
            .map(|s| s.trim())
        else {
            return Err(ClientKeyExtractError::MissingKey);
        };

        if key.is_empty() {
            return Err(ClientKeyExtractError::MissingKey);
        }

        if !client_keys.iter().any(|k| k == key) {
            return Err(ClientKeyExtractError::InvalidKey);
        }

        Ok(ClientKey)
    }
}

impl IntoResponse for ClientKeyExtractError {
    fn into_response(self) -> Response {
        match self {
            ClientKeyExtractError::InvalidKey => (StatusCode::FORBIDDEN, "Invalid client key"),
            ClientKeyExtractError::MissingKey => (StatusCode::UNAUTHORIZED, "Missing client key"),
        }
        .into_response()
    }
}
