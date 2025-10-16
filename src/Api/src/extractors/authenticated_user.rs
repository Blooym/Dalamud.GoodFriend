use crate::AppState;
use axum::{
    extract::{FromRef, FromRequestParts},
    http::{HeaderMap, StatusCode, request::Parts},
    response::{IntoResponse, Response},
};
use sha2::{Digest, Sha512};

const AUTH_TOKEN_HEADER: &str = "x-auth-token";

pub struct AuthenticatedUser;

#[derive(Debug)]
pub enum AuthenticationExtractError {
    MissingAuthToken,
    InvalidAuthToken,
}

impl<S> FromRequestParts<S> for AuthenticatedUser
where
    AppState: FromRef<S>,
    S: Send + Sync,
{
    type Rejection = AuthenticationExtractError;

    async fn from_request_parts(parts: &mut Parts, state: &S) -> Result<Self, Self::Rejection> {
        let state = AppState::from_ref(state);
        let headers: &HeaderMap = &parts.headers;
        let token = headers
            .get(AUTH_TOKEN_HEADER)
            .and_then(|v| v.to_str().ok())
            .map(str::trim)
            .filter(|s| !s.is_empty())
            .ok_or(AuthenticationExtractError::MissingAuthToken)?;

        let mut hasher = Sha512::new();
        hasher.update(token);
        let token_hash = hex::encode(hasher.finalize());

        if !state.authentication_tokens.contains(&token_hash) {
            return Err(AuthenticationExtractError::InvalidAuthToken);
        }

        Ok(AuthenticatedUser)
    }
}

impl IntoResponse for AuthenticationExtractError {
    fn into_response(self) -> Response {
        match self {
            AuthenticationExtractError::InvalidAuthToken => {
                (StatusCode::FORBIDDEN, "Invalid authentication token")
            }
            AuthenticationExtractError::MissingAuthToken => {
                (StatusCode::UNAUTHORIZED, "No authentication token")
            }
        }
        .into_response()
    }
}
