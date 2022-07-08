(async () => {
    const client = new composeMessaging.ComposeMessagingClient("ws://localhost:5098/ws");

    window.client = client;

    await client.connect();
    client.subscribe('testTopic', (message) => console.log('Received message on "testTopic"', message));
    
    client.publish('testTopic', { hello: 'world' });
})();
