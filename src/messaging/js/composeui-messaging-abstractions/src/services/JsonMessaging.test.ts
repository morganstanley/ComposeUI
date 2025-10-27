import { describe, it, expect, beforeEach, vi } from 'vitest';
import { JsonMessaging } from './JsonMessaging';
import { IMessaging } from './IMessaging';
import { ServiceHandler } from './Delegates';

describe('JsonMessaging', () => {
    let mockMessaging: IMessaging;
    let jsonMessaging: JsonMessaging;

    beforeEach(() => {
        mockMessaging = {
            subscribe: vi.fn(),
            publish: vi.fn(),
            registerService: vi.fn(),
            invokeService: vi.fn(),
        };
        jsonMessaging = new JsonMessaging(mockMessaging);
    });

    describe('subscribe and publish', () => {
        it('should delegate raw subscribe to underlying messaging', async () => {
            const handler = vi.fn();
            const unsubscribe = { unsubscribe: vi.fn() };
            (mockMessaging.subscribe as any).mockResolvedValue(unsubscribe);

            const result = await jsonMessaging.subscribe('topic', handler);

            expect(mockMessaging.subscribe).toHaveBeenCalledWith('topic', handler, undefined);
            expect(result).toBe(unsubscribe);
        });

        it('should handle JSON subscribe and publish', async () => {
            const payload = { data: 'test' };
            const handler = vi.fn();
            (mockMessaging.subscribe as any).mockImplementation(async (topic: string, callback: any) => {
                await callback(JSON.stringify(payload));
                return { unsubscribe: vi.fn() };
            });

            await jsonMessaging.subscribeJson('topic', handler);
            expect(handler).toHaveBeenCalledWith(payload);

            await jsonMessaging.publishJson('topic', payload);
            expect(mockMessaging.publish).toHaveBeenCalledWith('topic', JSON.stringify(payload), undefined);
        });
    });

    describe('service registration and invocation', () => {
        it('should handle JSON service registration', async () => {
            const handler = vi.fn().mockResolvedValue({ result: 'success' });
            const disposable = { dispose: vi.fn() };
            (mockMessaging.registerService as any).mockResolvedValue(disposable);

            const result = await jsonMessaging.registerJsonService('service', handler);

            expect(mockMessaging.registerService).toHaveBeenCalledWith('service', expect.any(Function), undefined);
            expect(result).toBe(disposable);
        });

        it('should handle JSON service invocation', async () => {
            const request = { data: 'test' };
            const response = { result: 'success' };
            (mockMessaging.invokeService as any).mockResolvedValue(JSON.stringify(response));

            const result = await jsonMessaging.invokeJsonService('service', request);

            expect(mockMessaging.invokeService).toHaveBeenCalledWith('service', JSON.stringify(request), undefined);
            expect(result).toEqual(response);
        });

        it('should handle null response from service', async () => {
            (mockMessaging.invokeService as any).mockResolvedValue(null);

            const result = await jsonMessaging.invokeJsonService('service', { data: 'test' });

            expect(result).toBeNull();
        });

        it('should handle service invocation with no request', async () => {
            const response = { result: 'success' };
            (mockMessaging.invokeService as any).mockResolvedValue(JSON.stringify(response));

            const result = await jsonMessaging.invokeJsonServiceNoRequest('service');

            expect(mockMessaging.invokeService).toHaveBeenCalledWith('service', null, undefined);
            expect(result).toEqual(response);
        });
    });

    describe('error handling', () => {
        it('should handle JSON parse errors in subscribe', async () => {
            const handler = vi.fn();
            (mockMessaging.subscribe as any).mockImplementation(async (topic: string, callback: any) => {
                await expect(callback('invalid json')).rejects.toThrow(SyntaxError);
                return { unsubscribe: vi.fn() };
            });

            await jsonMessaging.subscribeJson('topic', handler);
            expect(handler).not.toHaveBeenCalled();
        });

        it('should handle JSON parse errors in service handler', async () => {
            const handler = vi.fn();
            const disposable = { dispose: vi.fn() };
            (mockMessaging.registerService as any).mockResolvedValue(disposable);

            await jsonMessaging.registerJsonService('service', handler);
            const serviceHandler: ServiceHandler = (mockMessaging.registerService as any).mock.calls[0][1];
            
            await expect(serviceHandler('invalid json')).rejects.toThrow(SyntaxError);
            expect(handler).not.toHaveBeenCalled();
        });
    });
});
