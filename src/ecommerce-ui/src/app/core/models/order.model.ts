export type OrderStatus =
  | 'Pending'
  | 'ReservationConfirmed'
  | 'PaymentAuthorized'
  | 'Confirmed'
  | 'Shipped'
  | 'Delivered'
  | 'Cancelled'
  | 'Failed';

export interface OrderDto {
  id: string;
  customerId: string;
  customerEmail: string;
  status: OrderStatus;
  items: OrderItemDto[];
  totalAmount: number;
  shippingAddress: string;
  billingAddress: string;
  paymentMethod: string;
  cancellationReason: string | null;
  idempotencyKey: string;
  createdAt: string;
  updatedAt: string;
}

export interface OrderItemDto {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
}

export interface PlaceOrderRequest {
  customerEmail: string;
  items: PlaceOrderItemRequest[];
  shippingAddress: string;
  billingAddress: string;
  paymentMethod: string;
  idempotencyKey: string;
}

export interface PlaceOrderItemRequest {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
}
