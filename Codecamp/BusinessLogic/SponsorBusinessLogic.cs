﻿using Codecamp.Data;
using Codecamp.Models;
using Codecamp.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Codecamp.BusinessLogic
{
    public interface ISponsorBusinessLogic
    {
        Task<List<Sponsor>> GetAllSponsors();
        Task<List<SponsorViewModel>> GetAllSponsorsViewModel(bool loadImages = true);
        Task<Sponsor> GetSponsor(int sponsorId);
        Task<SponsorViewModel> GetSponsorViewModel(int sponsorId);
        Task<bool> SponsorExists(int sponsorId);
        Task<bool> CreateSponsor(Sponsor sponsor);
        Task<bool> UpdateSponsor(Sponsor sponsor);
        Task<bool> DeleteSponsor(int sponsorId);
        byte[] ResizeImage(byte[] originalImage);
    }

    public class SponsorBusinessLogic : ISponsorBusinessLogic
    {
        private CodecampDbContext _context { get; set; }

        public SponsorBusinessLogic(CodecampDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all sponsors
        /// </summary>
        /// <param name="context">List of Sponsor objects</param>
        public async Task<List<Sponsor>> GetAllSponsors()
        {
            return await _context.Sponsors.ToListAsync();
        }

        /// <summary>
        /// Get all SponsorViewModels
        /// </summary>
        /// <returns>List of SponsorViewModel objects</returns>
        public async Task<List<SponsorViewModel>> GetAllSponsorsViewModel(bool loadImages = true)
        {
            return await ToSponsorViewModel(_context.Sponsors, loadImages).ToListAsync();
        }

        /// <summary>
        /// Get the specified Sponsor
        /// </summary>
        /// <param name="sponsorId">The desired sponsor Id</param>
        /// <returns>The Sponsor object</returns>
        public async Task<Sponsor> GetSponsor(int sponsorId)
        {
            return await _context.Sponsors.FirstAsync(
                s => s.SponsorId == sponsorId);
        }

        /// <summary>
        ///  Get the specified SponsorViewModel
        /// </summary>
        /// <param name="sponsorId">The desired sponsor Id</param>
        /// <returns>The SponsorViewModel object</returns>
        public async Task<SponsorViewModel> GetSponsorViewModel(int sponsorId)
        {
            return ToSponsorViewModel(await _context.Sponsors.FirstAsync(s => s.SponsorId == sponsorId));
        }

        /// <summary>
        /// Does the specified sponsor exist?
        /// </summary>
        /// <param name="sponsorId">The specified sponsor Id</param>
        /// <returns>True or false indicating whether the sponsor exists</returns>
        public async Task<bool> SponsorExists(int sponsorId)
        {
            return await _context.Sponsors.AnyAsync(s => s.SponsorId == sponsorId);
        }

        /// <summary>
        /// Creates and adds the sponsor to the sponsor collection.
        /// </summary>
        /// <param name="sponsor">The sponsor to create and add</param>
        /// <returns>True or false indicating whether the sponsor was created</returns>
        public async Task<bool> CreateSponsor(Sponsor sponsor)
        {
            try
            {
                _context.Sponsors.Add(sponsor);

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception )
            {
                return false;
            }
        }

        /// <summary>
        /// Update the specified sponsor
        /// </summary>
        /// <param name="sponsor">The updated sponsor information</param>
        /// <returns>True or false indicating whether the update was successful</returns>
        public async Task<bool> UpdateSponsor(Sponsor sponsor)
        {
            try
            {
                _context.Sponsors.Update(sponsor);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await SponsorExists(sponsor.SponsorId))
                    return false;
                else
                    throw;
            }
        }

        /// <summary>
        /// Delete the specified sponsor
        /// </summary>
        /// <param name="sponsorId">The id of the desired sponsor</param>
        /// <returns>True or false indicating whether the delete was successful</returns>
        public async Task<bool> DeleteSponsor(int sponsorId)
        {
            try
            {
                var sponsor = await _context.Sponsors.FindAsync(sponsorId);
                if (sponsor != null)
                {
                    _context.Sponsors.Remove(sponsor);

                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Resize the supplied image file byte[] to 500px X 500px
        /// </summary>
        /// <param name="originalImage">The original image byte[]</param>
        /// <returns>The resized image byte[]</returns>
        public byte[] ResizeImage(byte[] originalImage)
        {
            const int size = 500; // max size in pixels

            if (originalImage != null)
            {
                MemoryStream imageStream = new MemoryStream(originalImage);
                using (var image = new Bitmap(imageStream))
                {
                    if (image.Width < size && image.Height < size)
                        return originalImage;

                    int width, height;
                    if (image.Width > image.Height)
                    {
                        width = size;
                        height = Convert.ToInt32(image.Height * size / (double)image.Width);
                    }
                    else
                    {
                        width = Convert.ToInt32(image.Width * size / (double)image.Height);
                        height = size;
                    }

                    var resized = new Bitmap(width, height);
                    using (var graphics = Graphics.FromImage(resized))
                    {
                        graphics.CompositingQuality = CompositingQuality.HighSpeed;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.CompositingMode = CompositingMode.SourceCopy;
                        graphics.DrawImage(image, 0, 0, width, height);

                        using (var ms = new MemoryStream())
                        {
                            var qualityParamId = Encoder.Quality;
                            var encoderParameters = new EncoderParameters(1);
                            encoderParameters.Param[0] = new EncoderParameter(qualityParamId, 100L);
                            var codec = ImageCodecInfo.GetImageDecoders()
                                .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
                            resized.Save(ms, codec, encoderParameters);

                            return ms.ToArray();
                        }
                    }
                }
            }

            return null;
        }

        private IQueryable<SponsorViewModel> ToSponsorViewModel(IQueryable<Sponsor> sponsors, bool loadImages = true)
        {
            IQueryable<SponsorViewModel> resultingSponsors;

            if (loadImages == true)
            {
                resultingSponsors = from sponsor in sponsors
                                    join _event in _context.Events on sponsor.EventId equals _event.EventId
                                    select new SponsorViewModel
                                    {
                                        SponsorId = sponsor.SponsorId,
                                        CompanyName = sponsor.CompanyName,
                                        SponsorLevel = sponsor.SponsorLevel,
                                        Bio = sponsor.Bio,
                                        TwitterHandle = sponsor.TwitterHandle,
                                        WebsiteUrl = sponsor.WebsiteUrl,
                                        PointOfContact = sponsor.PointOfContact,
                                        EmailAddress = sponsor.EmailAddress,
                                        PhoneNumber = sponsor.PhoneNumber,
                                        Image = sponsor.Image != null && sponsor.Image.Length > 0
                                            ? string.Format("data:image;base64,{0}", Convert.ToBase64String(sponsor.Image))
                                            : string.Empty,
                                        EventName = _event.Name
                                    };
            }
            else
            {
                resultingSponsors = from sponsor in sponsors
                                    join _event in _context.Events on sponsor.EventId equals _event.EventId
                                    select new SponsorViewModel
                                    {
                                        SponsorId = sponsor.SponsorId,
                                        CompanyName = sponsor.CompanyName,
                                        SponsorLevel = sponsor.SponsorLevel,
                                        Bio = sponsor.Bio,
                                        TwitterHandle = sponsor.TwitterHandle,
                                        WebsiteUrl = sponsor.WebsiteUrl,
                                        PointOfContact = sponsor.PointOfContact,
                                        EmailAddress = sponsor.EmailAddress,
                                        PhoneNumber = sponsor.PhoneNumber,
                                        EventName = _event.Name
                                    };
            }

            return resultingSponsors;
        }

        private SponsorViewModel ToSponsorViewModel(Sponsor sponsor)
        {
            var _event = _context.Events.FirstOrDefault(e => e.EventId == sponsor.EventId);

            var result = new SponsorViewModel
            {
                SponsorId = sponsor.SponsorId,
                CompanyName = sponsor.CompanyName,
                SponsorLevel = sponsor.SponsorLevel,
                Bio = sponsor.Bio,
                TwitterHandle = sponsor.TwitterHandle,
                WebsiteUrl = sponsor.WebsiteUrl,
                PointOfContact = sponsor.PointOfContact,
                EmailAddress = sponsor.EmailAddress,
                PhoneNumber = sponsor.PhoneNumber,
                Image = sponsor.Image != null && sponsor.Image.Length > 0
                    ? String.Format("data:image;base64,{0}", Convert.ToBase64String(sponsor.Image))
                    : string.Empty,
                EventName = _event != null ? _event.Name : string.Empty
            };

            return result;
        }
    }
}
