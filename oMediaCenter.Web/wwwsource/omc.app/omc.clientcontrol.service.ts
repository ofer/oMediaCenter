import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { ActivatedRoute, Router, Params } from '@angular/router';

import 'rxjs/add/operator/toPromise';
import { Observable } from 'rxjs/Rx';

import { SettingsService } from './omc.settings.service';
import { MediaFileRecord } from './omc.mediafilerecord.model';
import { ClientCommand } from './omc.clientCommand.model';
import { IPlayerControl } from './omc.playercontrol.interface';

@Injectable()
export class ClientControlService {

    private headers = new Headers({ 'Content-Type': 'application/json' });
    private clientCommandUrl = 'api/v1/client';  // URL to web api

    private clientId: string;
    private lastExecutedDate: Date;
    private playerControl: IPlayerControl;

    constructor(private http: Http,
        private router: Router,
        private settingsService: SettingsService) {
        this.lastExecutedDate = new Date(0);
    }

    startPolling() {
        this.createClientId().then(response => {
            console.log('received ' + response + ' as the client id');
            this.clientId = response;
            let timer = Observable.interval(5000);
            timer.subscribe(t => this.pollAndProcessCommands());
        });
    }

    pollAndProcessCommands() {
        this.getLatestCommands().then(command => {
            var commandDate = new Date(command.date);
            if (commandDate > this.lastExecutedDate) {
                this.executeCommand(command);
                this.lastExecutedDate = commandDate;
            }
        });
    }

    createClientId(): Promise<string> {
        return this.settingsService.getClientId();
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
                if (command.parameter)
                    this.router.navigate(['/media', command.parameter]);
                else
                    if (this.playerControl)
                        this.playerControl.onPlay();
                break;
            case 'pause':
                if (this.playerControl)
                    this.playerControl.onPause();
                break;
            case 'stop':
                if (this.playerControl)
                    this.playerControl.onStop();
                break;
            case 'volumeUp':
                if (this.playerControl)
                    this.playerControl.onVolumeUp();
                break;
            case 'volumeDown':
                if (this.playerControl)
                    this.playerControl.onVolumeDown();
                break;
            case 'toggleFullscreen':
                if (this.playerControl)
                    this.playerControl.onToggleFullscreen();
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

    setPlayer(playerControl: IPlayerControl) {
        this.playerControl = playerControl;
    }

    private handleError(error: any): Promise<any> {
        console.error('An error occurred', error); // for demo purposes only
        return Promise.reject(error.message || error);
    }
}
