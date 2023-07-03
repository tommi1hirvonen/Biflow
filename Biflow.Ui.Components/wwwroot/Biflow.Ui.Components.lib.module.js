function eventArgsCreator(event) {
    return null;
}

// Blazor JavaScript initializer
// https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/startup?view=aspnetcore-7.0#javascript-initializers
export function afterStarted(blazor) {
    // Register custom onsearch event type for text inputs with type="search".
    // Chrome and some other browsers add a clear icon ("X") at the end of the text input to clear the input.
    // This launches a non-standard event 'search'.
    // https://learn.microsoft.com/en-us/aspnet/core/blazor/components/event-handling?view=aspnetcore-7.0#custom-event-arguments
    blazor.registerCustomEventType('search', {
        createEventArgs: eventArgsCreator
    });
}