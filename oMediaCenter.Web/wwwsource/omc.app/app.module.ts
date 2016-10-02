import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { HttpModule } from '@angular/http';
//import { MaterialModule } from '@angular/material';

import { AppComponent } from './app.component';
import { MediaDataService } from './omc.media.service';
import { MediaListComponent } from './omc.medialist.component';
import { MediaPlayerComponent } from './omc.mediaplayer.component';
import { routing } from './app.routing'

@NgModule({
    imports: [
        BrowserModule,
        FormsModule,
        HttpModule,
//        MaterialModule.forRoot(),
        routing
    ],
    declarations: [
        AppComponent,
        MediaListComponent,
        MediaPlayerComponent
    ],
    providers: [MediaDataService],
    bootstrap: [AppComponent]
})
export class AppModule { }