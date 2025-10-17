use crate::{AppState, extractors::ClientKey};
use axum::{
    extract::State,
    response::{
        Sse,
        sse::{Event, KeepAlive},
    },
};
use core::convert::Infallible;
use tokio_stream::{Stream, StreamExt, wrappers::BroadcastStream};

pub async fn player_events_sse_handler(
    _client_key: ClientKey,
    State(state): State<AppState>,
) -> Sse<impl Stream<Item = Result<Event, Infallible>>> {
    let rx = state.player_events_stream.subscribe();
    let stream = BroadcastStream::new(rx).filter_map(|msg| match msg {
        Ok(data) => Some(Ok(Event::default().json_data(data).unwrap())),
        Err(_) => None,
    });
    Sse::new(stream).keep_alive(KeepAlive::default())
}
