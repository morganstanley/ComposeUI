import { describe, it, expect, beforeEach, vi } from 'vitest';
import { MessageRouterMessaging } from './MessageRouterMessaging';
import { MessageRouter } from "@morgan-stanley/composeui-messaging-client";
import { TopicMessageHandler } from "@morgan-stanley/composeui-messaging-abstractions";

describe('MessageRouterMessaging', () => {
    let mockMessageRouter: MessageRouter;
    let messaging: MessageRouterMessaging;

    beforeEach(() => {
        mockMessageRouter = {
            subscribe: vi.fn(),
            publish: vi.fn(),
            registerService: vi.fn(),
            invoke: vi.fn(),
            unregisterService: vi.fn()
        } as any;

        messaging = new MessageRouterMessaging(mockMessageRouter);
    });

    describe('subscribe', () => {
        it('should delegate subscribe to message router', async () => {
            const topic = 'test-topic';
            const handler: TopicMessageHandler = vi.fn();
            const unsubscribe = { unsubscribe: vi.fn() };
            
            (mockMessageRouter.subscribe as any).mockResolvedValue(unsubscribe);

            const result = await messaging.subscribe(topic, handler);

            expect(mockMessageRouter.subscribe).toHaveBeenCalledWith(topic, expect.any(Function));
            expect(result).toBe(unsubscribe);
        });

        it('should handle empty messages', async () => {
            const topic = 'test-topic';
            const handler = vi.fn();
            let storedCallback: Function = () => {};

            (mockMessageRouter.subscribe as any).mockImplementation((t: string, callback: Function) => {
                storedCallback = callback;
                return Promise.resolve({ unsubscribe: vi.fn() });
            });

            await messaging.subscribe(topic, handler);
            await storedCallback({ payload: null });

            expect(handler).not.toHaveBeenCalled();
        });
    });

    describe('publish', () => {
        it('should delegate publish to message router', async () => {
            const topic = 'test-topic';
            const message = 'test-message';

            await messaging.publish(topic, message);

            expect(mockMessageRouter.publish).toHaveBeenCalledWith(topic, message);
        });
    });

    describe('registerService', () => {
        it('should delegate service registration to message router', async () => {
            const serviceName = 'test-service';
            const handler = vi.fn().mockResolvedValue('response');

            const disposable = await messaging.registerService(serviceName, handler);

            expect(mockMessageRouter.registerService).toHaveBeenCalledWith(
                serviceName,
                expect.any(Function)
            );

            expect(disposable).toBeDefined();
            expect(typeof disposable[Symbol.asyncDispose]).toBe('function');
        });

        it('should handle service calls correctly', async () => {
            const serviceName = 'test-service';
            const handler = vi.fn().mockResolvedValue('response');
            let registeredHandler: Function | undefined;

            (mockMessageRouter.registerService as any).mockImplementation((name: string, callback: Function) => {
                registeredHandler = callback;
                return Promise.resolve();
            });

            await messaging.registerService(serviceName, handler);
            
            if (!registeredHandler) {
                throw new Error('Handler was not registered');
            }

            const result = await registeredHandler('endpoint', 'payload', { context: 'test' });
            
            expect(handler).toHaveBeenCalledWith('payload');
            expect(result).toBe('response');
        });

        it('should handle null payload in service call', async () => {
            const serviceName = 'test-service';
            const handler = vi.fn().mockResolvedValue('response');
            let registeredHandler: Function | undefined;

            (mockMessageRouter.registerService as any).mockImplementation((name: string, callback: Function) => {
                registeredHandler = callback;
                return Promise.resolve();
            });

            await messaging.registerService(serviceName, handler);
            
            if (!registeredHandler) {
                throw new Error('Handler was not registered');
            }

            const result = await registeredHandler('endpoint', null, { context: 'test' });
            
            expect(handler).toHaveBeenCalledWith(null);
            expect(result).toBe('response');
        });
    });

    describe('invokeService', () => {
        it('should handle service invocation with payload', async () => {
            const serviceName = 'test-service';
            const payload = 'test-payload';
            const response = 'test-response';

            (mockMessageRouter.invoke as any).mockResolvedValue(response);

            const result = await messaging.invokeService(serviceName, payload);

            expect(mockMessageRouter.invoke).toHaveBeenCalledWith(serviceName, payload);
            expect(result).toBe(response);
        });

        it('should handle service invocation without payload', async () => {
            const serviceName = 'test-service';
            const response = 'test-response';

            (mockMessageRouter.invoke as any).mockResolvedValue(response);

            const result = await messaging.invokeService(serviceName);

            expect(mockMessageRouter.invoke).toHaveBeenCalledWith(serviceName);
            expect(result).toBe(response);
        });

        it('should handle null response', async () => {
            const serviceName = 'test-service';

            (mockMessageRouter.invoke as any).mockResolvedValue(null);

            const result = await messaging.invokeService(serviceName, 'payload');

            expect(result).toBeNull();
        });
    });

    describe('AsyncDisposableWrapper', () => {
        it('should unregister service on dispose', async () => {
            const serviceName = 'test-service';
            const handler = vi.fn();

            const disposable = await messaging.registerService(serviceName, handler);
            await disposable[Symbol.asyncDispose]();

            expect(mockMessageRouter.unregisterService).toHaveBeenCalledWith(serviceName);
        });
    });
});
