/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */
import { Component } from '@angular/core';
import { Observable } from "rxjs";


import { ThemeOption } from "../../services/theme-services/ThemeOption";
import { ThemeService } from "../../services/theme-services/theme.service";

@Component({
  selector: 'app-sidenav',
  templateUrl: './sidenav.component.html',
  styleUrls: ['./sidenav.component.scss']
})
export class SidenavComponent {
  options: Observable<Array<ThemeOption>> = this.themeService.getThemeOptions();
  storedTheme = localStorage.getItem('selectedTheme')

  constructor(private readonly themeService: ThemeService) {}

  ngOnInit() {
    this.storedTheme ? this.themeChangeHandler(this.storedTheme) : this.themeChangeHandler('indigo-pink')
  }

  themeChangeHandler(themeToSet:any) {
    this.themeService.setTheme(themeToSet);
  }
}
