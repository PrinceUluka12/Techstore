import { useLocation, useNavigate } from 'react-router-dom'
import toast from 'react-hot-toast'
import { cartApi } from '../services/api'
import { useCartStore, useWishlistStore } from '../store'

// Signed-out shoppers who try to add to cart, save an item or check out are sent to sign in
// (or create an account), then brought back to where they were with that action done for them.
// It all travels in the router state of the /login (and /register) page:
//   from:    where to go afterwards
//   then:    { addToCart: { productId, quantity } } or { wishlist: productId }
//   message: shown above the sign-in form, e.g. "Sign in to add X to your cart."

export function useSignInFirst() {
  const navigate = useNavigate()
  const location = useLocation()
  return ({ then = null, message = null, to = null } = {}) =>
    navigate('/login', { state: { from: to ?? location.pathname + location.search, then, message } })
}

// The state to put on a plain "Sign in" link so it comes back to this page
export const returnHere = (location) => ({ from: location.pathname + location.search })

// After signing in or registering: do what they were trying to do, then take them back
export async function finishSignIn(state, navigate, fallback) {
  const then = state?.then
  try {
    if (then?.addToCart) {
      const { data } = await cartApi.addItem(then.addToCart)
      useCartStore.getState().setCart(data)
      toast.success('Added to your cart')
    }
    if (then?.wishlist && await useWishlistStore.getState().add(then.wishlist))
      toast.success('Saved to your wishlist')
  } catch (err) {
    toast.error(err.response?.data?.message || "We couldn't add that for you. Please try again.")
  }
  const from = state?.from
  navigate(from && !/^\/(login|register)/.test(from) ? from : fallback, { replace: true })
}
