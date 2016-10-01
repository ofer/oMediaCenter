import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { HttpModule } from '@angular/http';

import { AppComponent } from './app.component';
import { MediaDataService } from './omc.media.service';
import { MediaListComponent } from './omc.medialist.component';
import { routing } from './app.routing'

@NgModule({
    imports: [
        BrowserModule,
        FormsModule,
        HttpModule,
        routing
    ],
    declarations: [
        AppComponent,
        MediaListComponent
    ],
    providers: [MediaDataService],
    bootstrap: [AppComponent]
})
export class AppModule { }