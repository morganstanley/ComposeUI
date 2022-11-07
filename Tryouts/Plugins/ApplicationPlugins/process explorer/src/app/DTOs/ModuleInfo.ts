import { Byte } from "@angular/compiler/src/util";
import { Guid } from "igniteui-angular-core";

export class ModuleInfo{
    Version: Guid;
    Name: string;
    VersionRedirectedFrom: string;
    PublicKeyToken: Array<Byte>;
    Location: string;
    Information: Array<any>;
}