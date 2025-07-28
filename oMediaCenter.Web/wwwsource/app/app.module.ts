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
import { MatIconModule } from '@angular/material/icon';
import { AppRoutingModule } from './app-routing.module';
import { MediaDataService } from './media-data.service';
import { SettingsService } from './settings.service';
import { ClientControlService } from './client-control.service';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';

import { VgCoreModule } from '@videogular/ngx-videogular/core';
import { VgControlsModule } from '@videogular/ngx-videogular/controls';
import { VgOverlayPlayModule } from '@videogular/ngx-videogular/overlay-play';
import { VgBufferingModule } from '@videogular/ngx-videogular/buffering';
import { VgStreamingModule } from '@videogular/ngx-videogular/streaming';
import { MediaListPageComponent } from './media-list-page/media-list-page.component';
import { MatExpansionModule } from '@angular/material/expansion';
import { SeasonListExpansionComponent } from './season-list-expansion/season-list-expansion.component';
import { RecentlyPlayedPageComponent } from './recently-played-page/recently-played-page.component';

@NgModule({
    declarations: [
        AppComponent,
        ClientControlComponent,
        MediaListComponent,
        MediaPlayerComponent,
        SettingsComponent,
        MediaListPageComponent,
        SeasonListExpansionComponent,
        RecentlyPlayedPageComponent
    ],
    bootstrap: [AppComponent],
    imports: [BrowserModule,
        AppRoutingModule,
        FormsModule,
        BrowserAnimationsModule,
        MatGridListModule,
        MatButtonModule,
        MatListModule,
        MatIconModule,
        MatExpansionModule,
        VgCoreModule,
        VgControlsModule,
        VgOverlayPlayModule,
        VgBufferingModule,
        VgStreamingModule],

    providers: [
        MediaDataService,
        ClientControlService,
        SettingsService,
        provideHttpClient(withInterceptorsFromDi())
    ]
})
export class AppModule { }
