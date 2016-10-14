import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, Params } from '@angular/router';

import { ClientCommand } from './omc.clientCommand.model';
import { ClientControlService } from './omc.clientcontrol.service';

import { MediaFileRecord } from './omc.mediafilerecord.model';
import { MediaDataService } from './omc.media.service';


@Component({
    selector: 'omc-client-control',
    templateUrl: './omc.app/omc.clientcontrol.component.html',
})
export class ClientControlComponent implements OnInit {
    title = 'Client Remote Control';

    public mediaFileList: MediaFileRecord[];
    public selectedHost: string;
    public hosts: string[];

    constructor(
        private clientControlService: ClientControlService,
        private mediaDataService: MediaDataService
    ) {
        this.refreshHosts();
        mediaDataService.getMediaFileRecords().then(records => this.mediaFileList = records);
    }

    refreshHosts() {
        this.clientControlService.getAllHosts().then(hosts => this.hosts = hosts);
    }

    ngOnInit(): void {
    }

    onSendRequested(commandType, hash) {
        this.clientControlService.sendCommand(commandType, this.selectedHost, hash);
    }
}
