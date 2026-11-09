import { ApiUrl } from "./apiUrl";

export interface EmailAttachment {
  id: string;
  name: string;
  type: string | null;
  contentBytesBase64: string;
}

// subset
export interface EmailMessage {
  operationId: string,
  from: string,
  to: string,
  cc: string,
  bcc: string,
  subject: string,
  plainText: string,
  html: string,
  replyTo: string,
  attachments: EmailAttachment[],
  disableUserEngagementTracking: boolean
}

export const getAllEmails = async (): Promise<EmailMessage[]>  => {
  const response = await fetch(`${ApiUrl}/admin/emails`);  
  const data = await response.json();
  return data.value;
}