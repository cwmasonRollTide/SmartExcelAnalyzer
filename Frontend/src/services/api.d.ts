import { SubmitQueryResponse } from '../interfaces/SubmitQueryResponse';
export declare const uploadFile: (file: File) => Promise<string>;
export declare const submitQuery: (query: string, documentId: string) => Promise<SubmitQueryResponse>;
