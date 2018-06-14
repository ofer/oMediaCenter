export interface IPlayerControl {
	onStop(): void;
	onVolumeUp(): void;
	onVolumeDown(): void;
	onPause(): void;
	onPlay(): void;
	onToggleFullscreen(): void;
}
