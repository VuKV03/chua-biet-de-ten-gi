import { useState } from 'react'
import { Menu, Moon, Search, X } from 'lucide-react'
import logo from '../../assets/logo.png'

const navLinks = [
    { label: 'Home', href: '#home' },
    { label: 'About', href: '#about' },
    { label: 'Contact', href: '#contact' },
]

const Navbar = () => {
    const [open, setOpen] = useState(false)

    return (
        <header className='sticky top-0 z-50 px-3 py-3 sm:px-6 sm:py-4'>
            <nav className='relative mx-auto max-w-7xl rounded-2xl border border-neutral-200/80 bg-white shadow-[0_1px_3px_rgba(15,23,42,0.06)]'>
                <div className='flex h-16 items-center content-center justify-between gap-4 px-4 sm:px-6 md:grid md:grid-cols-[1fr_auto_1fr]'>
                    {/* Logo */}
                    <a href='/' className='flex shrink-0 items-center md:justify-self-start'>
                        <img
                            src={logo}
                            alt='Logo'
                            className='h-9 w-auto max-w-[150px] object-contain'
                        />
                    </a>

                    {/* Menu - desktop */}
                    <ul className='hidden items-center gap-8 md:flex md:justify-self-center'>
                        {navLinks.map((link) => (
                            <li key={link.label} className='flex'>
                                <a
                                    href={link.href}
                                    className='text-[15px] font-medium leading-none text-neutral-700 transition-colors hover:text-neutral-950'
                                >
                                    {link.label}
                                </a>
                            </li>
                        ))}
                    </ul>

                    {/* Hành động - desktop */}
                    <div className='hidden min-w-0 items-center gap-2 md:flex md:justify-self-end'>
                        {/* Search */}
                        <div className='flex h-9 min-w-0 items-center gap-2 rounded-full border border-neutral-200 bg-neutral-50 pl-3 pr-3 transition-colors focus-within:border-neutral-400 focus-within:bg-white'>
                            <Search className='size-4 shrink-0 text-neutral-400' strokeWidth={2} />
                            <input
                                type='text'
                                placeholder='Search...'
                                className='w-32 min-w-0 bg-transparent text-sm leading-none text-neutral-800 outline-none placeholder:text-neutral-400 lg:w-48'
                            />
                        </div>

                        {/* Theme */}
                        <button
                            type='button'
                            aria-label='Đổi giao diện sáng/tối'
                            className='grid size-9 shrink-0 place-items-center rounded-full border border-neutral-200 text-neutral-600 transition-colors hover:bg-neutral-100 hover:text-neutral-900'
                        >
                            <Moon className='size-4' strokeWidth={2} />
                        </button>
                    </div>

                    {/* Nút mở menu - mobile */}
                    <button
                        type='button'
                        onClick={() => setOpen((prev) => !prev)}
                        aria-label={open ? 'Đóng menu' : 'Mở menu'}
                        aria-expanded={open}
                        className='grid size-9 shrink-0 place-items-center rounded-lg text-neutral-700 transition-colors hover:bg-neutral-100 md:hidden'
                    >
                        {open ? <X className='size-5' /> : <Menu className='size-5' />}
                    </button>
                </div>

                {/* Menu - mobile */}
                {open && (
                    <div className='absolute inset-x-0 top-[calc(100%+0.5rem)] rounded-2xl border border-neutral-200/80 bg-white p-4 shadow-lg md:hidden'>
                        <div className='flex h-10 items-center gap-2 rounded-full border border-neutral-200 bg-neutral-50 px-3 transition-colors focus-within:border-neutral-400 focus-within:bg-white'>
                            <Search className='size-4 shrink-0 text-neutral-400' strokeWidth={2} />
                            <input
                                type='text'
                                placeholder='Search...'
                                className='min-w-0 flex-1 bg-transparent text-sm text-neutral-800 outline-none placeholder:text-neutral-400'
                            />
                        </div>

                        <ul className='mt-3 flex flex-col'>
                            {navLinks.map((link) => (
                                <li key={link.label}>
                                    <a
                                        href={link.href}
                                        onClick={() => setOpen(false)}
                                        className='block rounded-lg px-3 py-2.5 text-[15px] font-medium text-neutral-700 transition-colors hover:bg-neutral-50 hover:text-neutral-950'
                                    >
                                        {link.label}
                                    </a>
                                </li>
                            ))}
                        </ul>

                        <div className='mt-3 border-t border-neutral-200 pt-3'>
                            <button
                                type='button'
                                className='flex h-10 w-full items-center justify-center gap-2 rounded-full border border-neutral-200 text-sm font-medium text-neutral-700 transition-colors hover:bg-neutral-100'
                            >
                                <Moon className='size-4' strokeWidth={2} />
                                <span>Chế độ tối</span>
                            </button>
                        </div>
                    </div>
                )}
            </nav>
        </header>
    )
}

export default Navbar
