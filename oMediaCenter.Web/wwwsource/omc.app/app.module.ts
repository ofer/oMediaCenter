﻿import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { HttpModule } from '@angular/http';
import { MaterialModule, MdGridListModule, MdButtonModule } from '@angular/material';
//import { MdGridListModule } from '@angular/material/grid-list';

import { AppComponent } from './app.component';

import { ClientControlService } from './omc.clientcontrol.service';
import { ClientControlComponent } from './omc.clientcontrol.component';

import { MediaDataService } from './omc.media.service';
import { MediaListComponent } from './omc.medialist.component';
import { MediaPlayerComponent } from './omc.mediaplayer.component';
import { routing } from './app.routing'

@NgModule({
    imports: [
        BrowserModule,
        FormsModule,
        HttpModule,
        MaterialModule.forRoot(),
        MdGridListModule.forRoot(),
        MdButtonModule.forRoot(),
        routing
    ],
    declarations: [
        AppComponent,
        MediaListComponent,
        MediaPlayerComponent,
        ClientControlComponent
    ],
    providers: [
        MediaDataService,
        ClientControlService
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }