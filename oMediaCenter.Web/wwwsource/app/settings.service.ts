import { Injectable } from '@angular/core';
import { Http, Headers } from '@angular/http';

@Injectable()
export class SettingsService {

  private headers = new Headers({ 'Content-Type': 'application/json' });
  private clientCommandUrl = 'api/v1/client';  // URL to web api

  private clientId: string;

  constructor(private http: Http) {
  }

  getClientId() : Promise<string> {
  if (localStorage.getItem('clientId')) {
          this.clientId = localStorage.getItem('clientId');
          return new Promise<string>(r => r(this.clientId));
      } else {
          return this.http.post(this.clientCommandUrl, { ClientName: '' })
              .toPromise().then(response => {
                  var clientId = response.text();
                  localStorage.setItem('clientId', clientId);
                  return clientId;
              });
      }
  }

  setClientId(clientId: string) {
      localStorage.setItem('clientId', clientId);
  }

}
