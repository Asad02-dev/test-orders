export interface NotificationRecord {
  id: string;
  orderId: string;
  type: string;
  channel: string;
  recipientEmail: string;
  subject: string;
  body: string;
  status: string;
  sentAt: string | null;
  createdAt: string;
}
