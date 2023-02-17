/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { DatagridComponent } from './datagrid.component';

describe('DatagridComponent', () => {
  let component: DatagridComponent;
  let fixture: ComponentFixture<DatagridComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ DatagridComponent ],
      imports: [
        MatTableModule,
        MatPaginatorModule,
        BrowserAnimationsModule
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DatagridComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  
  it('it should create app', () => {
    const fixture = TestBed.createComponent(DatagridComponent);
    const app = fixture.componentInstance;
    console.log(app);
    fixture.detectChanges();
    expect(app).toBeTruthy();
  });
});
