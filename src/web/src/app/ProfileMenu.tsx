import { useEffect, useId, useRef, useState, type KeyboardEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation } from 'react-router'
import { Icon } from '../components/Icon'
import { useLogout } from '../features/auth/useLogout'

const itemClass =
  'flex min-h-target w-full items-center gap-3 px-4 text-left text-label text-ink no-underline focus-visible:bg-panel'

/**
 * The profile button at the far right of the header and its menu: Profile · Settings · Log out (T-109, D-101).
 * Shown on every teacher screen except sign-in. Before set-up is done (`/welcome…`) neither Profile nor Settings
 * can be opened yet, so the menu has Log out only. Decided by the path, not by `me`: the welcome screen's
 * hand-off to the evening step is timing-sensitive to who else watches that query.
 *
 * Keyboard: Enter or Space opens it with focus on the first item; the arrow keys, Home and End move between
 * items; Escape closes it and puts focus back on the button; Tab closes it and moves on.
 */
export function ProfileMenu() {
  const { t } = useTranslation()
  const { pathname } = useLocation()
  const { logout, pending } = useLogout()
  const [open, setOpen] = useState(false)
  const menuId = useId()
  const wrapper = useRef<HTMLDivElement>(null)
  const button = useRef<HTMLButtonElement>(null)
  const menu = useRef<HTMLDivElement>(null)

  // Opening puts focus on the first item, so the arrow keys work at once.
  useEffect(() => {
    if (open) {
      menu.current?.querySelector<HTMLElement>('[role="menuitem"]')?.focus()
    }
  }, [open])

  // A tap anywhere else closes it. Focus goes back to the button unless she tapped something that takes focus itself.
  // On click, not pointerdown: the browser moves focus on mousedown, which would undo a focus set any earlier.
  useEffect(() => {
    if (!open) {
      return
    }
    function onClick(event: MouseEvent) {
      const target = event.target
      if (!(target instanceof Node) || wrapper.current?.contains(target)) {
        return
      }
      setOpen(false)
      if (!(target instanceof Element) || !target.closest('a, button, input, select, textarea, [tabindex]')) {
        button.current?.focus()
      }
    }
    document.addEventListener('click', onClick)
    return () => document.removeEventListener('click', onClick)
  }, [open])

  // The sign-in screen is the only one a signed-out teacher can see.
  if (pathname === '/login') {
    return null
  }
  const setUp = !pathname.startsWith('/welcome')

  function onMenuKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    const items = Array.from(menu.current?.querySelectorAll<HTMLElement>('[role="menuitem"]') ?? [])
    const at = items.indexOf(document.activeElement as HTMLElement)
    const focus = (index: number) => {
      event.preventDefault()
      items[(index + items.length) % items.length]?.focus()
    }
    switch (event.key) {
      case 'ArrowDown':
        focus(at + 1)
        break
      case 'ArrowUp':
        focus(at - 1)
        break
      case 'Home':
        focus(0)
        break
      case 'End':
        focus(items.length - 1)
        break
      case 'Escape':
        event.preventDefault()
        setOpen(false)
        button.current?.focus()
        break
      case 'Tab':
        setOpen(false)
        break
    }
  }

  async function onLogout() {
    setOpen(false)
    // Offline she stays signed in (the announcer says so): give her the button back.
    if (!(await logout())) {
      button.current?.focus()
    }
  }

  return (
    <div ref={wrapper} className="relative shrink-0">
      <button
        ref={button}
        type="button"
        aria-label={t('profile.menuLabel')}
        aria-haspopup="menu"
        aria-expanded={open}
        aria-controls={open ? menuId : undefined}
        onClick={() => setOpen((was) => !was)}
        className="flex size-target items-center justify-center rounded-full border-2 border-border bg-surface text-ink"
      >
        <Icon name="user" size={22} />
      </button>
      {open && (
        <div
          ref={menu}
          id={menuId}
          role="menu"
          tabIndex={-1}
          aria-label={t('profile.menuLabel')}
          onKeyDown={onMenuKeyDown}
          className="absolute right-0 top-full z-20 mt-1 w-max min-w-44 max-w-[calc(100vw-2rem)] overflow-hidden rounded-button border-2 border-border bg-surface py-1"
        >
          {setUp && (
            <>
              <Link to="/profile" role="menuitem" tabIndex={-1} onClick={() => setOpen(false)} className={itemClass}>
                <Icon name="user" size={20} />
                {t('profile.title')}
              </Link>
              <Link to="/settings" role="menuitem" tabIndex={-1} onClick={() => setOpen(false)} className={itemClass}>
                <Icon name="settings" size={20} />
                {t('settings.title')}
              </Link>
            </>
          )}
          <button
            type="button"
            role="menuitem"
            tabIndex={-1}
            disabled={pending}
            onClick={() => void onLogout()}
            className={itemClass}
          >
            <Icon name="logOut" size={20} />
            {t('auth.logout')}
          </button>
        </div>
      )}
    </div>
  )
}
