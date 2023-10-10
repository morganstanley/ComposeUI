/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */
import { Injectable } from '@angular/core';
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";

import { ThemeOption } from "../../DTOs/ThemeOption";
import { StyleManagerService } from './style-manager.service';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {

  constructor(private http: HttpClient, private styleManager: StyleManagerService) { }

  getThemeOptions(): Observable<Array<ThemeOption>> {
    return this.http.get<Array<ThemeOption>>("assets/options.json");
  }

  setTheme(themeToSet: any) {
    this.styleManager.setStyle(
      "theme",
      `${themeToSet}.css`
    );
    localStorage.setItem('selectedTheme', `${themeToSet}`)
  }
}
