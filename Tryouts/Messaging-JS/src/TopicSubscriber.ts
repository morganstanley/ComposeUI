import { PartialObserver } from "rxjs";
import { TopicMessage } from ".";

export type TopicSubscriber = PartialObserver<TopicMessage>;
