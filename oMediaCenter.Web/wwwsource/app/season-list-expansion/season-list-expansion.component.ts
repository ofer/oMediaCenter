import { Component, OnInit, Input, EventEmitter, Output } from '@angular/core';
import { MediaFileRecord } from '../media-file-record';

@Component({
  selector: 'app-season-list-expansion',
  templateUrl: './season-list-expansion.component.html',
  styleUrls: ['./season-list-expansion.component.css']
})
export class SeasonListExpansionComponent implements OnInit {
  @Input() seasonFileList: MediaFileRecord[] | null = null;
  @Output() mediaSelected: EventEmitter<string> = new EventEmitter<string>();

  shouldShowFileList: boolean = false;

  constructor() { }

  ngOnInit(): void {
  }

  toggleShowFileList() {
    this.shouldShowFileList = !this.shouldShowFileList;
  }

  onSelect(media: MediaFileRecord) {
    this.mediaSelected.emit(media.hash);
    // this.router.navigate(['/media', media.hash]);
  }

}
