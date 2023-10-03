import { Component, EventEmitter, Input, Output } from "@angular/core";

import { Option } from "../../DTOs/Option";
import { ThemeService } from "../../services/theme-services/theme.service";

@Component({
  selector: 'app-theme-selector',
  templateUrl: './theme-selector.component.html',
  styleUrls: ['./theme-selector.component.scss']
})
export class ThemeSelectorComponent {
  @Input() options: Array<Option> | null
  @Output() themeChange: EventEmitter<string> = new EventEmitter<string>();

  changeTheme(themeToSet: any) {
    this.themeChange.emit(themeToSet);
  }
}
