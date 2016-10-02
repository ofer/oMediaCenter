import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';

import 'rxjs/add/operator/toPromise';

import { MediaFileRecord } from './omc.mediafilerecord.model';

@Injectable()
export class MediaDataService {

    private headers = new Headers({ 'Content-Type': 'application/json' });
    private mediaInfoUrl = 'api/v1/media';  // URL to web api

    constructor(private http: Http) { }

    getMediaFileRecords(): Promise<MediaFileRecord[]> {
        return this.http.get(this.mediaInfoUrl)
            .toPromise()
            .then(response =>
                response.json() as MediaFileRecord[])
            .catch(this.handleError);
    }

    getMediaFileRecord(hash: string): Promise<MediaFileRecord> {
        return this.getMediaFileRecords()
            .then(records =>
                records.find(record =>
                    record.hash === hash));
    }

    delete(hash: number): Promise<void> {
        let url = `${this.mediaInfoUrl}/${hash}`;
        return this.http.delete(url, { headers: this.headers })
            .toPromise()
            .then(() => null)
            .catch(this.handleError);
    }

    private handleError(error: any): Promise<any> {
        console.error('An error occurred', error); // for demo purposes only
        return Promise.reject(error.message || error);
    }
}



/*
Copyright 2016 Google Inc. All Rights Reserved.
Use of this source code is governed by an MIT-style license that
can be found in the LICENSE file at http://angular.io/license
*/