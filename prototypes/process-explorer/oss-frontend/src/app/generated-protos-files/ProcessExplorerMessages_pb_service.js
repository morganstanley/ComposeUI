// package: 
// file: ProcessExplorerMessages.proto

var ProcessExplorerMessages_pb = require("./ProcessExplorerMessages_pb");
var google_protobuf_empty_pb = require("google-protobuf/google/protobuf/empty_pb");
var grpc = require("@improbable-eng/grpc-web").grpc;

var ProcessExplorerMessageHandler = (function () {
  function ProcessExplorerMessageHandler() {}
  ProcessExplorerMessageHandler.serviceName = "ProcessExplorerMessageHandler";
  return ProcessExplorerMessageHandler;
}());

ProcessExplorerMessageHandler.Send = {
  methodName: "Send",
  service: ProcessExplorerMessageHandler,
  requestStream: false,
  responseStream: false,
  requestType: ProcessExplorerMessages_pb.Message,
  responseType: google_protobuf_empty_pb.Empty
};

ProcessExplorerMessageHandler.Subscribe = {
  methodName: "Subscribe",
  service: ProcessExplorerMessageHandler,
  requestStream: false,
  responseStream: true,
  requestType: google_protobuf_empty_pb.Empty,
  responseType: ProcessExplorerMessages_pb.Message
};

exports.ProcessExplorerMessageHandler = ProcessExplorerMessageHandler;

function ProcessExplorerMessageHandlerClient(serviceHost, options) {
  this.serviceHost = serviceHost;
  this.options = options || {};
}

ProcessExplorerMessageHandlerClient.prototype.send = function send(requestMessage, metadata, callback) {
  if (arguments.length === 2) {
    callback = arguments[1];
  }
  var client = grpc.unary(ProcessExplorerMessageHandler.Send, {
    request: requestMessage,
    host: this.serviceHost,
    metadata: metadata,
    transport: this.options.transport,
    debug: this.options.debug,
    onEnd: function (response) {
      if (callback) {
        if (response.status !== grpc.Code.OK) {
          var err = new Error(response.statusMessage);
          err.code = response.status;
          err.metadata = response.trailers;
          callback(err, null);
        } else {
          callback(null, response.message);
        }
      }
    }
  });
  return {
    cancel: function () {
      callback = null;
      client.close();
    }
  };
};

ProcessExplorerMessageHandlerClient.prototype.subscribe = function subscribe(requestMessage, metadata) {
  var listeners = {
    data: [],
    end: [],
    status: []
  };
  var client = grpc.invoke(ProcessExplorerMessageHandler.Subscribe, {
    request: requestMessage,
    host: this.serviceHost,
    metadata: metadata,
    transport: this.options.transport,
    debug: this.options.debug,
    onMessage: function (responseMessage) {
      listeners.data.forEach(function (handler) {
        handler(responseMessage);
      });
    },
    onEnd: function (status, statusMessage, trailers) {
      listeners.status.forEach(function (handler) {
        handler({ code: status, details: statusMessage, metadata: trailers });
      });
      listeners.end.forEach(function (handler) {
        handler({ code: status, details: statusMessage, metadata: trailers });
      });
      listeners = null;
    }
  });
  return {
    on: function (type, handler) {
      listeners[type].push(handler);
      return this;
    },
    cancel: function () {
      listeners = null;
      client.close();
    }
  };
};

exports.ProcessExplorerMessageHandlerClient = ProcessExplorerMessageHandlerClient;

