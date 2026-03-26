export type PaymentStatus =
  | 'Pending'
  | 'Authorized'
  | 'Captured'
  | 'Failed'
  | 'Refunded';

export interface PaymentDto {
  id: string;
  orderId: string;
  amount: number;
  currency: string;
  status: PaymentStatus;
  paymentMethod: string;
  transactionId: string | null;
  failureReason: string | null;
  createdAt: string;
  updatedAt: string;
}
