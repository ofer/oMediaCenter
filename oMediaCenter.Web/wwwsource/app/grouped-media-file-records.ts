import { MediaFileRecord } from './media-file-record';

export interface GroupedMediaFileRecords {
    name: string;
    description: string;
    mediaFileRecords: MediaFileRecord[];
    thumbnailType: string;
    genres: string;
  }
  