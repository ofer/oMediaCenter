import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';

@Injectable()
export class SettingsService {

  private headers = new HttpHeaders({ 'Content-Type': 'application/json' });
  private clientCommandUrl = 'api/v1/client';  // URL to web api

  private clientId!: string | null;

  constructor(private http: HttpClient) {
  }

  getClientId(): string | null {

    return localStorage.getItem('clientId');
    // if (localStorage.getItem('clientId')) {
    //   this.clientId = localStorage.getItem('clientId');
    //   return new Promise<string | null>(r => r(this.clientId));
    // } else {
    //   return this.http.post(this.clientCommandUrl, { ClientName: '' })
    //     .toPromise().then(response => {
    //       if (response) {
    //         var clientId = response as string;
    //         localStorage.setItem('clientId', clientId);
    //         return clientId;
    //       }
    //       return null;
    //     });
    // }
  }

  setClientId(clientId: string) {
    localStorage.setItem('clientId', clientId);
  }

}
