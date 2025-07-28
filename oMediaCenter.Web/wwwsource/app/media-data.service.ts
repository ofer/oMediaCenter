import { Injectable } from '@angular/core';
import { HttpHeaders, HttpClient } from '@angular/common/http';
import { MediaFileRecord } from './media-file-record';
import { GroupedMediaFileRecords } from './grouped-media-file-records';

@Injectable()
export class MediaDataService {

    private headers = new HttpHeaders({ 'Content-Type': 'application/json' });
    private mediaInfoUrl = 'api/v1/media';  // URL to web api

    constructor(private http: HttpClient) { }

    getGroupedMediaFileRecords(): Promise<GroupedMediaFileRecords[]> {
        return this.http.get(this.mediaInfoUrl)
            .toPromise()
            .then(response => {
                console.log('getGroupedMediaFileRecords received records');
                return this.groupToTopLevel(response as MediaFileRecord[]);
            })
            .catch(this.handleError);
    }


    groupToTopLevel(fileRecords: MediaFileRecord[]): GroupedMediaFileRecords[] {
        let result: { [key: string]: GroupedMediaFileRecords } = {};
        fileRecords.forEach(mfr => {
            if (!result[mfr.name]) {
                result[mfr.name] = {
                    description: mfr.description,
                    genres: mfr.genres,
                    mediaFileRecords: [],
                    name: mfr.name,
                    thumbnailType: ''
                };
            }
            result[mfr.name].mediaFileRecords.push(mfr);
        });

        return Object.values(result).flat();
    }

    getGroupedMediaFileRecord(hash: string): Promise<GroupedMediaFileRecords | undefined> {
        return this.getGroupedMediaFileRecords()
            .then(records => {
                return records.find(record => record.mediaFileRecords.some(mfr => mfr.hash === hash));
            });
    }

    getMediaFileRecord(hash: string): Promise<MediaFileRecord> {
        return this.getGroupedMediaFileRecords()
            .then(records => {
                for (const groupedRecord of records) {
                    let candidate = groupedRecord.mediaFileRecords.find(mfr => mfr.hash === hash);
                    if (candidate) {
                        return candidate as MediaFileRecord;
                    }
                }
                throw new Error(`MediaFileRecord with hash ${hash} not found`);
            });
    }

    // getGroupedFileRecords(): Promise<GroupedMediaFileRecords[]> {
    //     return this.getMediaFileRecords()
    //         .then(records => {
    //             // find tv shows to group

    //         });

    // }

    updateMediaCurrentTime(hash: string, currentTime: number) {
        return this.http.put(this.mediaInfoUrl + '/' + hash, { CurrentTime: currentTime }).toPromise();
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
