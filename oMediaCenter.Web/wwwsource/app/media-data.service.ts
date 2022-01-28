import { Injectable } from '@angular/core';
import { HttpHeaders, HttpClient } from '@angular/common/http';
import { MediaFileRecord } from './media-file-record';

@Injectable()
export class MediaDataService {

  private headers = new HttpHeaders({ 'Content-Type': 'application/json' });
  private mediaInfoUrl = 'api/v1/media';  // URL to web api

  constructor(private http: HttpClient) { }
  
  getMediaFileRecords(): Promise<MediaFileRecord[]> {
      return this.http.get(this.mediaInfoUrl)
          .toPromise()
          .then(response =>
              response as MediaFileRecord[])
          .catch(this.handleError);
  }

  getMediaFileRecord(hash: string): Promise<MediaFileRecord> {
      return this.getMediaFileRecords()
          .then(records =>
            records.find(record =>
              record.hash === hash) as MediaFileRecord);
  }

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
