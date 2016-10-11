import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { ActivatedRoute, Router, Params } from '@angular/router';

import 'rxjs/add/operator/toPromise';
import { Observable } from 'rxjs/Rx';

import { MediaFileRecord } from './omc.mediafilerecord.model';
import { ClientCommand } from './omc.clientCommand.model';

@Injectable()
export class ClientControlService {

    private headers = new Headers({ 'Content-Type': 'application/json' });
    private clientCommandUrl = 'api/v1/client';  // URL to web api

    private clientId: string;
    private lastExecutedDate: Date;

    constructor(private http: Http,
        private router: Router) {
        this.lastExecutedDate = new Date(0);
    }

    startPolling() {
        this.createClientId().then(response => {
            this.clientId = response;
            let timer = Observable.interval(5000);
            timer.subscribe(t => this.pollAndProcessCommands());
        });
    }

    pollAndProcessCommands() {
        this.getLatestCommands().then(command => {
            if (new Date(command.date) > this.lastExecutedDate) {
                this.executeCommand(command);
                this.lastExecutedDate = command.date;
            }
        });
    }

    createClientId(): Promise<string> {
        return this.http.post(this.clientCommandUrl, { ClientName: localStorage.getItem('clientName') })
            .toPromise().then(response => response.text());
    }

    getLatestCommands(): Promise<ClientCommand> {
        return this.http.get(this.clientCommandUrl + '/' + this.clientId)
            .toPromise()
            .then(response =>
                response.json() as ClientCommand)
            .catch(this.handleError);
    }

    executeCommand(command: ClientCommand) {
        switch (command.command) {
            case 'play':
                this.router.navigate(['/media', command.parameter]);
                break;
            case 'pause':
                this.router.navigate(['/media', command.parameter]);
                break;
            case 'index':
                this.router.navigate(['/medialist']);
                break;
        }
    }

    getAllHosts() {
        return this.http.get(this.clientCommandUrl)
            .toPromise()
            .then(response => response.json() as string[]);
    }

    sendCommand(commandType: string, host: string, hash: string) {
        let command = new ClientCommand(commandType, hash, new Date());

        return this.http.put(this.clientCommandUrl + '/' + host, command).toPromise();
    }


    private handleError(error: any): Promise<any> {
        console.error('An error occurred', error); // for demo purposes only
        return Promise.reject(error.message || error);
    }
}
