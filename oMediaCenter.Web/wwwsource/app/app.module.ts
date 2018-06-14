import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { AppComponent } from './app.component';
import { ClientControlComponent } from './client-control/client-control.component';
import { MediaListComponent } from './media-list/media-list.component';
import { MediaPlayerComponent } from './media-player/media-player.component';
import { SettingsComponent } from './settings/settings.component';

import { MatGridListModule, MatButtonModule } from '@angular/material';
import { AppRoutingModule } from './app-routing.module';
import { MediaDataService } from './media-data.service';
import { SettingsService } from './settings.service';
import { ClientControlService } from './client-control.service';
import { HttpModule } from '@angular/http';

@NgModule({
  declarations: [
    AppComponent,
    ClientControlComponent,
    MediaListComponent,
    MediaPlayerComponent,
    SettingsComponent
  ],
  imports: [
    BrowserModule,
    HttpModule,
    AppRoutingModule,
    FormsModule,
    BrowserAnimationsModule,
    MatGridListModule,
    MatButtonModule
  ],
  providers: [
    MediaDataService,
    ClientControlService,
    SettingsService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
