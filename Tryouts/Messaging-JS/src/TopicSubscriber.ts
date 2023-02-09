import { PartialObserver } from "rxjs";
import { TopicMessage } from "./TopicMessage";

export type TopicSubscriber = PartialObserver<TopicMessage> | ((message: TopicMessage) => void);
