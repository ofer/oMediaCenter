import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { AppComponent } from './app.component';
import { ClientControlComponent } from './client-control/client-control.component';
import { MediaListComponent } from './media-list/media-list.component';
import { MediaPlayerComponent } from './media-player/media-player.component';
import { SettingsComponent } from './settings/settings.component';

import { MatGridListModule } from '@angular/material/grid-list';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { AppRoutingModule } from './app-routing.module';
import { MediaDataService } from './media-data.service';
import { SettingsService } from './settings.service';
import { ClientControlService } from './client-control.service';
import { HttpClientModule } from '@angular/common/http';

import { VgCoreModule } from '@videogular/ngx-videogular/core';
import { VgControlsModule } from '@videogular/ngx-videogular/controls';
import { VgOverlayPlayModule } from '@videogular/ngx-videogular/overlay-play';
import { VgBufferingModule } from '@videogular/ngx-videogular/buffering';
import { VgStreamingModule } from '@videogular/ngx-videogular/streaming';

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
    HttpClientModule,
    AppRoutingModule,
    FormsModule,
    BrowserAnimationsModule,
    MatGridListModule,
    MatButtonModule,
    MatListModule,
    VgCoreModule,
    VgControlsModule,
    VgOverlayPlayModule,
    VgBufferingModule,
    VgStreamingModule
  ],
  providers: [
    MediaDataService,
    ClientControlService,
    SettingsService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
