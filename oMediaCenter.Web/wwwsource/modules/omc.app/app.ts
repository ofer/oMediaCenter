import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { MediaListComponent } from './components/omc.medialistcomponent';
import { MediaDataService } from './services/omc.mediadataservice';

@NgModule({
    imports: [BrowserModule],
    declarations: [MediaListComponent],
    providers: [MediaDataService],
    bootstrap: [MediaListComponent]
})
export class AppModule { }