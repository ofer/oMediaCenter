import { Component } from '@angular/core';
import { MediaDataService } from '../services/omc.mediadataservice';

@Component({
    selector: 'omc-medialist',
    templateUrl: './modules/omc.app/components/omc.medialistcomponent.html'
})
export class MediaListComponent {
    title = 'Tour of zeros';
    _mediaFileList = {};

    constructor(
        private _mediaDataService: MediaDataService
    ) {}

    ngOnInit(): void {
        this._mediaFileList = this._mediaDataService.getMediaFileRecords();
    }
}
