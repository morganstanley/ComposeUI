// package: 
// file: ProcessExplorerMessages.proto

import * as ProcessExplorerMessages_pb from "./ProcessExplorerMessages_pb";
import * as google_protobuf_empty_pb from "google-protobuf/google/protobuf/empty_pb";
import {grpc} from "@improbable-eng/grpc-web";

type ProcessExplorerMessageHandlerSend = {
  readonly methodName: string;
  readonly service: typeof ProcessExplorerMessageHandler;
  readonly requestStream: false;
  readonly responseStream: false;
  readonly requestType: typeof ProcessExplorerMessages_pb.Message;
  readonly responseType: typeof google_protobuf_empty_pb.Empty;
};

type ProcessExplorerMessageHandlerSubscribe = {
  readonly methodName: string;
  readonly service: typeof ProcessExplorerMessageHandler;
  readonly requestStream: false;
  readonly responseStream: true;
  readonly requestType: typeof google_protobuf_empty_pb.Empty;
  readonly responseType: typeof ProcessExplorerMessages_pb.Message;
};

export class ProcessExplorerMessageHandler {
  static readonly serviceName: string;
  static readonly Send: ProcessExplorerMessageHandlerSend;
  static readonly Subscribe: ProcessExplorerMessageHandlerSubscribe;
}

export type ServiceError = { message: string, code: number; metadata: grpc.Metadata }
export type Status = { details: string, code: number; metadata: grpc.Metadata }

interface UnaryResponse {
  cancel(): void;
}
interface ResponseStream<T> {
  cancel(): void;
  on(type: 'data', handler: (message: T) => void): ResponseStream<T>;
  on(type: 'end', handler: (status?: Status) => void): ResponseStream<T>;
  on(type: 'status', handler: (status: Status) => void): ResponseStream<T>;
}
interface RequestStream<T> {
  write(message: T): RequestStream<T>;
  end(): void;
  cancel(): void;
  on(type: 'end', handler: (status?: Status) => void): RequestStream<T>;
  on(type: 'status', handler: (status: Status) => void): RequestStream<T>;
}
interface BidirectionalStream<ReqT, ResT> {
  write(message: ReqT): BidirectionalStream<ReqT, ResT>;
  end(): void;
  cancel(): void;
  on(type: 'data', handler: (message: ResT) => void): BidirectionalStream<ReqT, ResT>;
  on(type: 'end', handler: (status?: Status) => void): BidirectionalStream<ReqT, ResT>;
  on(type: 'status', handler: (status: Status) => void): BidirectionalStream<ReqT, ResT>;
}

export class ProcessExplorerMessageHandlerClient {
  readonly serviceHost: string;

  constructor(serviceHost: string, options?: grpc.RpcOptions);
  send(
    requestMessage: ProcessExplorerMessages_pb.Message,
    metadata: grpc.Metadata,
    callback: (error: ServiceError|null, responseMessage: google_protobuf_empty_pb.Empty|null) => void
  ): UnaryResponse;
  send(
    requestMessage: ProcessExplorerMessages_pb.Message,
    callback: (error: ServiceError|null, responseMessage: google_protobuf_empty_pb.Empty|null) => void
  ): UnaryResponse;
  subscribe(requestMessage: google_protobuf_empty_pb.Empty, metadata?: grpc.Metadata): ResponseStream<ProcessExplorerMessages_pb.Message>;
}

