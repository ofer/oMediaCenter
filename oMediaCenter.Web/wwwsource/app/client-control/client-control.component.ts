import { Component, OnInit } from '@angular/core';
import { MediaDataService } from '../media-data.service';
import { MediaFileRecord } from '../media-file-record';
import { ClientControlService } from '../client-control.service';
import { GroupedMediaFileRecords } from '../grouped-media-file-records';

@Component({
  selector: 'app-client-control',
  templateUrl: './client-control.component.html',
  styleUrls: ['./client-control.component.css']
})
export class ClientControlComponent implements OnInit {
  title = 'Client Remote Control';

  public mediaFileList!: GroupedMediaFileRecords[];
  public selectedHost!: string;
  public hosts!: string[];

  constructor(
    private clientControlService: ClientControlService,
    private mediaDataService: MediaDataService
  ) {
    this.refreshHosts();
    mediaDataService.getGroupedMediaFileRecords().then(records => this.mediaFileList = records);
  }

  refreshHosts() {
    this.clientControlService.getAllHosts().then(hosts => this.hosts = hosts);
  }

  ngOnInit(): void {
  }

  onSendRequested(commandType: string, hash: string | null) {
    this.clientControlService.sendCommand(commandType, this.selectedHost, hash);
  }
}
