import { Injectable } from '@angular/core';
import { ActivatedRoute, Router, Params } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { ClientCommand } from './client-command';
import { IPlayerControl } from './i-player-control';
import { SettingsService } from './settings.service';
import { Observable } from 'rxjs';
import { timer } from 'rxjs';
import * as signalR from '@microsoft/signalr';

@Injectable()
export class ClientControlService {

    private headers = new Headers({ 'Content-Type': 'application/json' });
    private clientCommandUrl = 'api/v1/client';  // URL to web api

    private clientId!: string;
    private lastExecutedDate: Date;
    private playerControl!: IPlayerControl;
    private connection: signalR.HubConnection;

    constructor(private http: HttpClient,
        private router: Router,
        private settingsService: SettingsService) {
        this.lastExecutedDate = new Date(0);
        this.connection = new signalR.HubConnectionBuilder()
        .configureLogging(signalR.LogLevel.Information)
        .withUrl('/commandHub')
        .build();
    }

    startPolling() {

        let connection = this.connection;
        let settingService = this.settingsService;

        this.connection.start().then(function () {
            console.log('SignalR Connected!');
            connection.invoke("RegisterClient", settingService.getClientId());
        }).catch(function (err) {
            return console.error(err.toString());
        });

        this.connection.on('CommandReceived', (clientCommand) => {
            console.log('received ' + clientCommand + ' via signalR');
            this.executeCommand(clientCommand);
        });

        this.connection.on('ClientIdGenerated', (generatedClientId) => {
            console.log('client ID was generated for us ' + generatedClientId);
            this.settingsService.setClientId(generatedClientId);
        });


        // this.createClientId().then(response => {
        //     console.log('received ' + response + ' as the client id');
        //     this.clientId = response as string;
        //     let pollTimer = timer(5000, 5000);
        //     pollTimer.subscribe(t => this.pollAndProcessCommands());
        // });
        
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

    getLatestCommands(): Promise<ClientCommand> {
        return this.http.get(this.clientCommandUrl + '/' + this.clientId)
            .toPromise()
            .then(response =>
                response as ClientCommand)
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
            .then(response => response as string[]);
    }

    sendCommand(commandType: string, host: string, hash: string | null) {
        let command: ClientCommand = { command: commandType, parameter: hash, date: new Date() };

        this.connection.invoke("SendCommand", host, command);

//        return this.http.put(this.clientCommandUrl + '/' + host, command).toPromise();
    }

    setPlayer(playerControl: IPlayerControl) {
        this.playerControl = playerControl;
    }

    private handleError(error: any): Promise<any> {
        console.error('An error occurred', error); // for demo purposes only
        return Promise.reject(error.message || error);
    }
}
