export class ClientCommand {
    constructor(
        /// <summary>
        /// A short name to describe the media (i. e. movie name)
        /// </summary>
        public command: string,

        /// <summary>
        /// A more detailed description of the media (i. e. plot summary)
        /// </summary>
        public parameter: string,
        public date: Date) { }
}
